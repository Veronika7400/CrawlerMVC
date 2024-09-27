document.addEventListener('DOMContentLoaded', () => {
    fetchProducts();

    document.getElementById('prevPage').classList.add('disabled');

    document.getElementById('prevPage').addEventListener('click', () => {
        let currentPage = parseInt(document.querySelector('input[name="page"]').value);
        if (currentPage > 1) {
            updatePage(currentPage - 1);
        }
    });

    document.getElementById('nextPage').addEventListener('click', () => {
        let currentPage = parseInt(document.querySelector('input[name="page"]').value);
        updatePage(currentPage + 1);
    });

    document.getElementById('pageSizeSelect').addEventListener('change', (event) => {
        updatePageSize(event.target.value);
    });

    document.getElementById("subscribeButton").addEventListener("click", () => {
        addSubscriber();
    });
});

function updatePage(newPage) {
    document.querySelector('input[name="page"]').value = newPage;
    document.getElementById('currentPage').innerText = newPage;
    fetchProducts();
}

function updatePageSize(newPageSize) {
    document.querySelector('input[name="page"]').value = 1;
    document.querySelector('select[name="pageSize"]').value = newPageSize;
    document.getElementById('currentPage').innerText = 1;
    fetchProducts();
}

function showLoader() {
    document.getElementById('loader').style.display = 'flex';
}

function hideLoader() {
    document.getElementById('loader').style.display = 'none';
}

function fetchProducts() {
    showLoader();

    const searchWord = document.getElementById('searchWord').value;
    const page = document.querySelector('input[name="page"]').value;
    const pageSize = document.querySelector('select[name="pageSize"]').value;
    const store = document.getElementById('storeId').value;

    const url = `/Price/ProductListSpecificStoreReturnList?searchWord=${searchWord}&store=${store}&page=${page}&pageSize=${pageSize}`;
    fetch(url)
        .then(response => {
            if (!response.ok) {
                return response.text().then(text => { throw new Error(text) });
            }
            return response.json();
        })
        .then(data => {
            updateProductList(data);
            const currentPage = parseInt(page);

            if (currentPage === 1) {
                document.getElementById('prevPage').classList.add('disabled');
            } else {
                document.getElementById('prevPage').classList.remove('disabled');
            }

            const productsCount = document.getElementById('productsContainer').children.length;
            if (productsCount < pageSize) {
                document.getElementById('nextPage').classList.add('disabled');
                hideLoader();
                return;
            }

            const nextPage = currentPage + 1;
            const nextUrl = `/Price/ProductListSpecificStoreReturnList?searchWord=${searchWord}&store=${store}&page=${nextPage}&pageSize=${pageSize}`;

            fetch(nextUrl)
                .then(response => response.json())
                .then(nextData => {
                    if (nextData.length === 0) {
                        document.getElementById('nextPage').classList.add('disabled');
                    } else {
                        document.getElementById('nextPage').classList.remove('disabled');
                    }
                    hideLoader();
                })
                .catch(error => {
                    document.getElementById('nextPage').classList.remove('disabled');
                    hideLoader();
                });
        })
        .catch(error => {
            console.error('Error fetching products:', error);
            hideLoader();
        });

function updateProductList(products) {
    const productsContainer = document.getElementById('productsContainer');
    productsContainer.innerHTML = '';

    if (products && products.length > 0) {
        products.forEach(product => {
            const productCard = document.createElement('div');
            productCard.classList.add('col');
            productCard.innerHTML = `
                <div class="card shadow-sm">
                    <div class="product-info">
                        <div class="product-image-container">
                            <img src="${product.imageUrl}" style="width: 300px; height: 300px;" aria-label="Product Image" alt="Product Image">
                        </div>
                        <div class="product-details">
                            <h5>${product.storeName}</h5>
                            <p class="productName">${product.productName}</p>
                            <p class="price">${product.priceValue} €</p>
                        </div>
                    </div>
                    <a href="${product.url}" class="view-page">View Page</a>
                </div>
            `;
            productsContainer.appendChild(productCard);
        });
    } else {
        productsContainer.innerHTML = '<p>No products found.</p>';
    }
    } 
}

function addSubscriber() {
    const frequency = document.getElementById("subscriptionFrequencySelect").value;
    const searchWord = document.getElementById("searchWord").value;
    const storeId = document.getElementById("storeId").value; 
    
    const url = `/Subscribers/AddSubscriber?frequency=${frequency}&searchWord=${searchWord}&notificationTypeName=Email&subscriptionTargetName=SpecificStore&storeId=${storeId}`;

    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({})
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                displaySuccessMessage('Action successfully completed.');
            } else if (data.denied) {
                displayDenyedMessage('You are already subscribed.');
            } else if (data.error) {
                displayErrorMessage('Action was not successfully completed.');
            }
        })
        .catch(error => {
            console.error('Error adding subscriber:', error);
            displayErrorMessage('Action was not successfully completed.');
        });
}

function displaySuccessMessage(message) {
    const successMessage = document.querySelector('.successMessage');
    successMessage.innerText = message;
    successMessage.style.display = 'block';
    setTimeout(() => {
        successMessage.style.display = 'none';
    }, 3000);
}

function displayErrorMessage(message) {
    const errorMessage = document.querySelector('.errorMessage');
    errorMessage.innerText = message;
    errorMessage.style.display = 'block';
    setTimeout(() => {
        errorMessage.style.display = 'none';
    }, 3000);
}

function displayDenyedMessage(message) {
    const errorMessage = document.querySelector('.denyedMessage');
    errorMessage.innerText = message;
    errorMessage.style.display = 'block';
    setTimeout(() => {
        errorMessage.style.display = 'none';
    }, 3000);
}